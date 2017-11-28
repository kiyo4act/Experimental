(function (global, $) {
    //-------------------- Difinitons ---------------------//

    // Points Array
    var g_arrayPoints;

    // Select Route Mode
    var g_selectedMode;

    // State oriantation
    var g_stateOrientation;

    // Static strings
    var g_strPortrait = "Portrait";
    var g_strLandscape = "Landscape";
    var g_strLogLevelNone = "none";
    var g_strLogLevelInfo = "info";
    var g_strLogLevelDebug = "debug";

    // Log Level
    var g_strLogLevel;

    var directionsDisplay;
    var directionsService = new google.maps.DirectionsService();
    var map;
    var fromA = new google.maps.LatLng(35.457572, 139.633298);
    //-------------------- Difinitons ---------------------//

    // Initialize function
    function initialize() {
        "use strict";
        directionsDisplay = new google.maps.DirectionsRenderer({
            "map": map,
            "draggable": true
        });
        var mapOptions = {
            zoom: 14,
            center: fromA
        };
        directionsDisplay.setPanel($("#result").get(0));
        map = new google.maps.Map($("#map-canvas").get(0), mapOptions);
        directionsDisplay.setMap(map);

        // Default selected mode
        g_selectedMode = "DRIVING";

        // Check default screen orientation


        // Detect change orientation
        /*$(window).bind("resize", function (e) {
            Logging("resize bind", "Begin function");
            changeLayout(checkOrientation());
            Logging("resize bind", "End function");
        });*/

        // Change Mode of Travel button Listener
        $(".kd-button").click(function () {
            Logging(".kd-button click listener", "begin");
            $(".kd-button").removeClass("selected");
            $(this).addClass("selected");
            g_selectedMode = $(this).attr("value");
            Logging(".kd-button click listener", g_selectedMode);

            // Invoke Search Button
            $("#search-button").click();
        });

        // Search button Listener
        $("#search-button").click(function invokeSearchButton() {
            "use strict";
            Logging("#search-button click listener", "begin");

            var fResult = false;

            // arrangement

            // Counting the number of points
            var searchInputs = $("#search-inputs > div");
            var numSearchInputs = searchInputs.length;

            // Create new array
            g_arrayPoints = [];

            // Get LatLng
            $.each(searchInputs, function (i) {
                var strLocation = $("#search-point-" + i + " > input").val();
                // Checking text area exist
                if (strLocation) {
                    getLatLng(strLocation, i);
                } else {
                    // If point is not exist 2points at least
                    if (i < 2) {
                        return fResult;
                    }
                }

                getLatLng(strLocation, i);
            });

            // Check finishing getLatLng's callback function
            alertLatLng(numSearchInputs);

            fResult = true;
            return fResult;
        });

        $("#search-inputs input").keydown(function (e) {
            if (e.keyCode === 13) { // Enter
                $("#search-button").click();
            }
        });
    }

    // Logging Function
    function Logging(funcname, description) {
        "use strict";
        if (g_strLogLevel === g_strLogLevelInfo) {
            console.log(Date() + ": " + funcname + " |> " + description);
        }
    }

    // Get LatLng
    function getLatLng(strAddress, pointnum) {
        "use strict";
        Logging("getLatLng", "Begin function");
        Logging("getLatLng", "strAddress = " + strAddress);
        Logging("getLatLng", "pointnum = " + pointnum);
        var geocoder = new google.maps.Geocoder();
        geocoder.geocode({
            'address': strAddress
        }, function (results, status) {
            Logging("[Callback]geocoder", "Begin function");
            Logging();
            if (status === google.maps.GeocoderStatus.OK) {
                if (results[0].geometry) {
                    g_arrayPoints[pointnum] = results[0].geometry.location;
                    Logging("[Callback]geocoder", "GetPoint: " + g_arrayPoints[pointnum].toString());
                }
            }
            Logging("[Callback]geocoder", "End function");
        });
        Logging("getLatLng", "End function");
    }

    // Waiting until point is not undefined
    function alertLatLng(length) {
        "use strict";
        Logging("alertLatLng", "Begin function");
        Logging("alertLatLng", "length = " + length);
        var repeat = function () {
            Logging("repeat", "Begin function");

            // Check undefined or not in g_arrayPoints
            var fCheckLatLng = true;
            //var i;
            //for (i = 0; i < length; i++) {
            $.each(Array(length), function (i) {
                if ("undefined" === typeof (g_arrayPoints[i])) {
                    fCheckLatLng = false;
                    Logging("[repeat]alertLatLng", "arrayPoint[" + i + "] is undefined");
                    Logging("[repeat]alertLatLng", "fCheckLatLng = ", +fCheckLatLng);
                    Logging("[repeat]alertLatLng", "break");
                    return false;
                } else {
                    Logging("[repeat]alertLatLng", "arrayPoint[" + i + "] is LatLng");
                }
            });

            if (fCheckLatLng) {
                Logging("[repeat]alertLatLng", "Call calcRoute");
                calcRoute(length);
                Logging("[repeat]alertLatLng", "Clear Interval");
                clearInterval(timerId);
            }

            Logging("[repeat]alertLatLng", "End function");
        }
        var timerId;
        timerId = setInterval(repeat, 100);
        Logging("alertLatLng", "End function");
    }


    // Calc Route
    function calcRoute(length) {
        "use strict";
        Logging("calcRoute", "Begin function");
        Logging("calcRoute", "length = " + length);

        // If points are exist more than 3
        var waypoints = [];
        if (length >= 3) {
            //var i;
            //for (i = 1; i < (length - 1); i++) {
            $.each(Array(length - 1), function (i) {
                if (g_arrayPoints[i] != "") {
                    waypoints.push({
                        location: g_arrayPoints[i],
                        stopover: true
                    });
                }
            });
        }

        var request = {
            origin: g_arrayPoints[0],
            destination: g_arrayPoints[(length - 1)],
            waypoints: waypoints,
            travelMode: google.maps.TravelMode[g_selectedMode]
        };
        directionsService.route(request, function (response, status) {
            if (status === google.maps.DirectionsStatus.OK) {
                directionsDisplay.setDirections(response);
            }
        });
        Logging("calcRoute", "End function");
    }

    /*
    // Check orientation Portrait or Landscape
    function checkOrientation() {
        Logging("checkOrientation", "Begin function");
        // Check Width vs Height
        var stateOrientation = (($(window).width() - $(window).height()) < 0 ? g_strPortrait : g_strLandscape);
        Logging("checkOrientation", "stateOrientation = " + stateOrientation);
        Logging("checkOrientation", "End function");
        return stateOrientation;
    }

    // Change Layout attach orientation
    function changeLayout(stateOrientation) {
        Logging("changeLayout", "Begin function");
        var fResult = false;
        if (g_stateOrientation !== stateOrientation) {
            Logging("changeLayout", "Layout will change");

            if (stateOrientation === g_strLandscape) {
                Logging("changeLayout", "stateOrientation === g_strLandscape");
                changeLayoutToLandscape();
            }
            if (stateOrientation === g_strPortrait) {
                Logging("changeLayout", "stateOrientation === g_strPortrait");
                changeLayoutToPortrait();
            }

            g_stateOrientation = stateOrientation;
            fResult = true;
        }
        Logging("changeLayout", "End function");
        return fResult;
    }

    function changeLayoutToLandscape() {
        Logging("changeLayoutToLandscape", "Begin function");
        $("#left").css({
            height: "100%",
            width: "50%"
        });
        $("#right").css({
            height: "calc(100% - 20px)",
            width: "calc(50% - 20px)",
            top: "0%",
            left: "50%"
        });
        Logging("changeLayoutToLandscape", "End function");
    }

    function changeLayoutToPortrait() {
        Logging("changeLayoutToPortrait", "Begin function");
        $("#left").css({
            height: "calc(50% - 20px)",
            width: "100%"
        });
        $("#right").css({
            height: "calc(50% - 0px)",
            width: "calc(100% - 20px)",
            top: "calc(50% - 20px)",
            left: "0%"
        });
        Logging("changeLayoutToPortrait", "End function");
    }

    // DOMContentoaded
    $(function () {
        Logging("DOMContentoaded", "Begin function");
        // Default CSS is Landscape mode
        g_stateOrientation = g_strLandscape;

        // Check orientation and Change Layout
        changeLayout(checkOrientation());
        Logging("DOMContentoaded", "End function");
    });
    */

    google.maps.event.addDomListener(global, 'load', initialize);
})(this, jQuery);