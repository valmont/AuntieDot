( function ( ) {
    'use strict';
    //Build a pseudo-class to prevent polluting our own scope.    
    var api = {
        Settings: { },
        Vox: { },
        Start: function ( ) {
            //Get the *.myshopify.com domain        
            var shop = Shopify.shop;
            //Load the store owner's widget settings        
            api.LoadSettings( shop,
                function ( settings ) {
                    //Save app settings            
                    api.Settings = settings;
                    //Load Riddlevox            
                    api.LoadRiddlevox( function ( ) {
                        //Configure Riddlevox                
                        api.Vox = api.ConfigureRiddlevox( api.Settings, api.SubmitHandler );
                        //Show the widget!                
                        api.Vox.Open( );
                    } );
                } );
        },
        ExecuteJSONP: function ( url, parameters, callback ) {
            //Prepare a function name that will be called when the JSONP request has loaded.        
            //It should be unique, to prevent accidentally calling it multiple times.        
            var callbackName = 'emailWidgetJSONPCallback' + new Date( ).getMilliseconds( );
            //Make the callback function available to the global scope,        
            //otherwise it can't be called when the settings are loaded.        
            window[ callbackName ] = callback;
            //Convert the parameters into a querystring        
            var kvps = [ 'jsonp=' + callbackName ];
            var keys = Object.getOwnPropertyNames( parameters );
            for ( var i = 0; i < keys.length; i++ ) {
                var key = keys[ i ];
                var value = parameters[ key ];
                if ( value.constructor === Array ) {
                    //Convert arrays to a string value that ASP.NET can read from the queryst ring.                
                    for ( var arrayIndex = 0; arrayIndex < value.length; arrayIndex++ ) {
                        kvps.push( key + '[' + arrayIndex + ']=' + value[ arrayIndex ] );
                    }
                } else {
                    kvps.push( key + '=' + value );
                }
            }
            //Add a unique parameter to the querystring, to overcome browser caching.        
            kvps.push( 'uid=' + new Date( ).getMilliseconds( ) );
            var qs = '?' + kvps.join( '&' );
            //Build the script element, passing along the shop name and the load function's n ame        
            var script = document.createElement( 'script' );
            script.src = url + qs;
            script.async = true;
            script.type = 'text/javascript';
            //Append the script to the document's head to execute it.        
            document.head.appendChild( script );
        },
        LoadSettings: function ( shop, callback ) {
            //Prepare a function to handle when the settings are loaded.        
            var settingsLoaded = function ( settings ) {
                //Return the settings to the Start function so it can continue loading.            
                callback( settings );
            };
            //Get the settings        
            api.ExecuteJSONP( 'https://auntiedot.apphb.com/widget/settings', { shop: shop }, settingsLoaded );
        },
        LoadRiddlevox: function ( callback ) {
            //Build the CSS element        
            var style = document.createElement( 'link' );
            style.href = 'https://ironstorage.blob.core.windows.net/public-downloads/Riddlevox/Riddlevox.css';
            style.rel = 'stylesheet';
            //Build the script element        
            var script = document.createElement( 'script' );
            script.src = 'https://ironstorage.blob.core.windows.net/public-downloads/Riddlevox/Riddlevox.js';
            script.async = true;
            script.type = 'text/javascript';
            //Set the script's onload event to the callback, so api.Start can continue after Riddlevox has loaded.        
            script.onload = callback;
            //Append the script and style to the document's head.        
            document.head.appendChild( script );
            document.head.appendChild( style );
        },
        ConfigureRiddlevox: function ( settings, submitHandler ) {
            var options = {
                Title: settings.Title,
                Message: settings.Blurb,
                BackgroundColor: settings.HexColor,
                ButtonText: 'Give me my free discount!',
                OnConversion: submitHandler,
                ThankYouMessage: 'Thank you! Please check your email address.'
            };
            //Initialize and start riddlevox        
            return new Riddlevox( options ).Start( );
        },
        SubmitHandler: function ( firstName, emailAddress ) {
            if ( !firstName || !emailAddress ) {
                api.Vox.ShowError( 'You must enter a valid name and email address.' );
                return;
            };
            //Create a callback function to handle successfully saving the visitor's email in formation.        
            var informationSaved = function ( result ) {
                //Show Riddlevox's thank-you message            
                api.Vox.ShowThankYouMessage( );
            };
            //Build the request's parameters        
            var params = { shop: Shopify.shop, firstName: firstName, emailAddress: emailAddress };
            //Make the request        
            api.ExecuteJSONP( 'https://auntiedot.apphb.com/widget/save', params, informationSaved );
        }
    };
    //Start the widget    
    api.Start( );
    //Optionally make the api available to the global scope for debugging.    
    window[ 'emailWidget' ] = api;
} )( );