/*jshint strict:false */
'use strict';
//https://github.com/mateu-aguilo-bosch/message-center/blob/master/message-center.js
// Create a new angular module.
var MessageCenterModule = angular.module('MessageCenterModule', []);

// Define a service to inject.
MessageCenterModule.
  service('messageService', ['$rootScope', '$sce', '$timeout',
    function ($rootScope, $sce, $timeout) {
        return {
            mcMessages: this.mcMessages || [],
            mcMessage: "",
            offlistener: this.offlistener || undefined,
            status: {
                unseen: 'unseen',
                shown: 'shown',
                /** @var Odds are that you will show a message and right after that
                 * change your route/state. If that happens your message will only be
                 * seen for a fraction of a second. To avoid that use the "next"
                 * status, that will make the message available to the next page */
                next: 'next',
                /** @var Do not delete this message automatically. */
                permanent: 'permanent'
            },
            info: function (message) {
                this.mcMessages = [];
                this.add("info", message);
            },
            warning: function (message) {
                this.mcMessages = [];
                this.add("warning", message);
            },
            danger: function (message) {
                this.mcMessages = [];
                this.add("danger", message);
            },
            success: function (message) {
                this.mcMessages = [];
                this.add("success", message);
            },
            successNext: function (message) {
                this.mcMessages = [];
                this.add("success", message, { "status": "next" });
            },
            highlight: function (message) {
                this.mcMessages = [];
                this.add("highlight", message);
            },
            add: function (type, message, options) {
  
                var availableTypes = ['info', 'warning', 'danger', 'success', 'highlight'],
                  service = this;
                options = options || {};
                if (availableTypes.indexOf(type) == -1) {
                    throw "Invalid message type";
                }
                var messageObject = {
                    type: type,
                    status: options.status || this.status.unseen,
                    processed: false,
                    close: function () {
                        return service.remove(this);
                    }
                };
              //  messageObject.message = options.html ? $sce.trustAsHtml(message) : message;
                messageObject.message = $sce.trustAsHtml(message);
               // this.mcMessage = $sce.trustAsHtml(message);
                messageObject.html = !!options.html;
                if (angular.isDefined(options.timeout)) {
                    messageObject.timer = $timeout(function () {
                        messageObject.close();
                    }, options.timeout);
                }
              //  this.mcMessage = messageObject;
                this.mcMessages.push(messageObject);
                this.flush();
                return messageObject;
            },
            remove: function (message) {
                var index = this.mcMessages.indexOf(message);
                this.mcMessages.splice(index, 1);
            },
            reset: function () {
                this.mcMessages = [];
            },
            removeShown: function () {
                for (var index = this.mcMessages.length - 1; index >= 0; index--) {
                    if (this.mcMessages[index].status == this.status.shown) {
                        this.remove(this.mcMessages[index]);
                    }
                }
            },
            markShown: function () {
                for (var index = this.mcMessages.length - 1; index >= 0; index--) {
                    if (!this.mcMessages[index].processed) {
                        if (this.mcMessages[index].status == this.status.unseen) {
                            this.mcMessages[index].status = this.status.shown;
                            this.mcMessages[index].processed = true;
                        }
                        else if (this.mcMessages[index].status == this.status.next) {
                            this.mcMessages[index].status = this.status.unseen;
                        }
                    }
                }
            },
            flush: function () {
                $rootScope.mcMessages = this.mcMessages;
            }
        };
    }
  ]);
MessageCenterModule.
  directive('mcMessages', ['$rootScope', 'messageService', function ($rootScope, messageService) {
      /*jshint multistr: true */
      var templateString = '\
<div ng-repeat="message in mcMessages">\
      <div class="alert alert-{{ message.type }}" ng-bind-html="message.message">\
      </div>\
 </div>\
    ';
      return {
          restrict: 'EA',
          template: templateString,
          link: function (scope, element, attrs) {
              // Bind the messages from the service to the root scope.
          
              messageService.flush();
              var changeReaction = function (event, to, from) {
         
                  // Update 'unseen' messages to be marked as 'shown'.
                  messageService.markShown();
                  // Remove the messages that have been shown.
                  messageService.removeShown();
                  $rootScope.mcMessages = messageService.mcMessages;
                 // $rootScope.mcMessage = messageService.mcMessage;
                  messageService.flush();
              };
              if (messageService.offlistener === undefined) {
                  messageService.offlistener = $rootScope.$on('$locationChangeSuccess', changeReaction);
              }
              scope.animation = attrs.animation || 'fade in';
          }
      };
  }]);