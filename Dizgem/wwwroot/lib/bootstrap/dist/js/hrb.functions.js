
(function ($) {
    $.fn.inputFilter = function (inputFilter) {
        return this.on("input keydown keyup mousedown mouseup select contextmenu drop", function () {
            if (inputFilter(this.value)) {
                this.oldValue = this.value;
                this.oldSelectionStart = this.selectionStart;
                this.oldSelectionEnd = this.selectionEnd;
            } else if (this.hasOwnProperty("oldValue")) {
                this.value = this.oldValue;
                this.setSelectionRange(this.oldSelectionStart, this.oldSelectionEnd);
            }
        });
    };
    $.fn.once = function (events, callback) {
        return this.each(function () {
            var myCallback = function (e) {
                callback.call(this, e);
                $(this).off(events, myCallback);
            };
            $(this).on(events, myCallback);
        });
    };


    if (!Array.prototype.where) {

        Array.prototype.where = function (filter) {

            var collection = this;

            switch (typeof filter) {

                case 'function':
                    return $.grep(collection, filter);

                case 'object':
                    for (var property in filter) {
                        if (!filter.hasOwnProperty(property))
                            continue; // ignore inherited properties

                        collection = $.grep(collection, function (item) {
                            return item[property] === filter[property];
                        });
                    }
                    return collection.slice(0); // copy the array
                // (in case of empty object filter)

                default:
                    throw new TypeError('func must be either a' +
                        'function or an object of properties and values to filter by');
            }
        };
    }



    Array.prototype.firstOrDefault = function (func) {
        return this.where(func)[0] || null;
    };

    $(document).on('focus blur', "[class*='inputmask'], .stack, .decimal", function () {
        $(this).once("click keyup", function (e) {
            $(this).select();
        });
    });

}(jQuery));

function isEmptyOrSpaces(str) {
    return str === null || str.match(/^ *$/) !== null;
}

function b64DecodeUnicode(str) {
    // Going backwards: from bytestream, to percent-encoding, to original string.
    return decodeURIComponent(atob(str).split('').map(function (c) {
        return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
    }).join(''));
}

function generateUUID() { // Public Domain/MIT
    var d = new Date().getTime();//Timestamp
    var d2 = ((typeof performance !== 'undefined') && performance.now && (performance.now() * 1000)) || 0;//Time in microseconds since page-load or 0 if unsupported
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        var r = Math.random() * 16;//random number between 0 and 16
        if (d > 0) {//Use timestamp until depleted
            r = (d + r) % 16 | 0;
            d = Math.floor(d / 16);
        } else {//Use microseconds since page-load if supported
            r = (d2 + r) % 16 | 0;
            d2 = Math.floor(d2 / 16);
        }
        return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
}