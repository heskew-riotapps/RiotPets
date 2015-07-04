app.config(['$translateProvider', function ($translateProvider) {
    $translateProvider
      .translations('en', enTranslations)
      .preferredLanguage('en');
}]);