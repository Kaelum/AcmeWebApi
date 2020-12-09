/*
   Licensed to the Apache Software Foundation (ASF) under one or more
   contributor license agreements.  See the NOTICE file distributed with
   this work for additional information regarding copyright ownership.
   The ASF licenses this file to You under the Apache License, Version 2.0
   (the "License"); you may not use this file except in compliance with
   the License.  You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
*/
var showControllersOnly = false;
var seriesFilter = "";
var filtersOnlySampleSeries = true;

/*
 * Add header in statistics table to group metrics by category
 * format
 *
 */
function summaryTableHeader(header) {
    var newRow = header.insertRow(-1);
    newRow.className = "tablesorter-no-sort";
    var cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 1;
    cell.innerHTML = "Requests";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 3;
    cell.innerHTML = "Executions";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 7;
    cell.innerHTML = "Response Times (ms)";
    newRow.appendChild(cell);

    cell = document.createElement('th');
    cell.setAttribute("data-sorter", false);
    cell.colSpan = 2;
    cell.innerHTML = "Network (KB/sec)";
    newRow.appendChild(cell);
}

/*
 * Populates the table identified by id parameter with the specified data and
 * format
 *
 */
function createTable(table, info, formatter, defaultSorts, seriesIndex, headerCreator) {
    var tableRef = table[0];

    // Create header and populate it with data.titles array
    var header = tableRef.createTHead();

    // Call callback is available
    if(headerCreator) {
        headerCreator(header);
    }

    var newRow = header.insertRow(-1);
    for (var index = 0; index < info.titles.length; index++) {
        var cell = document.createElement('th');
        cell.innerHTML = info.titles[index];
        newRow.appendChild(cell);
    }

    var tBody;

    // Create overall body if defined
    if(info.overall){
        tBody = document.createElement('tbody');
        tBody.className = "tablesorter-no-sort";
        tableRef.appendChild(tBody);
        var newRow = tBody.insertRow(-1);
        var data = info.overall.data;
        for(var index=0;index < data.length; index++){
            var cell = newRow.insertCell(-1);
            cell.innerHTML = formatter ? formatter(index, data[index]): data[index];
        }
    }

    // Create regular body
    tBody = document.createElement('tbody');
    tableRef.appendChild(tBody);

    var regexp;
    if(seriesFilter) {
        regexp = new RegExp(seriesFilter, 'i');
    }
    // Populate body with data.items array
    for(var index=0; index < info.items.length; index++){
        var item = info.items[index];
        if((!regexp || filtersOnlySampleSeries && !info.supportsControllersDiscrimination || regexp.test(item.data[seriesIndex]))
                &&
                (!showControllersOnly || !info.supportsControllersDiscrimination || item.isController)){
            if(item.data.length > 0) {
                var newRow = tBody.insertRow(-1);
                for(var col=0; col < item.data.length; col++){
                    var cell = newRow.insertCell(-1);
                    cell.innerHTML = formatter ? formatter(col, item.data[col]) : item.data[col];
                }
            }
        }
    }

    // Add support of columns sort
    table.tablesorter({sortList : defaultSorts});
}

$(document).ready(function() {

    // Customize table sorter default options
    $.extend( $.tablesorter.defaults, {
        theme: 'blue',
        cssInfoBlock: "tablesorter-no-sort",
        widthFixed: true,
        widgets: ['zebra']
    });

    var data = {"OkPercent": 99.04837205884093, "KoPercent": 0.9516279411590685};
    var dataset = [
        {
            "label" : "KO",
            "data" : data.KoPercent,
            "color" : "#FF6347"
        },
        {
            "label" : "OK",
            "data" : data.OkPercent,
            "color" : "#9ACD32"
        }];
    $.plot($("#flot-requests-summary"), dataset, {
        series : {
            pie : {
                show : true,
                radius : 1,
                label : {
                    show : true,
                    radius : 3 / 4,
                    formatter : function(label, series) {
                        return '<div style="font-size:8pt;text-align:center;padding:2px;color:white;">'
                            + label
                            + '<br/>'
                            + Math.round10(series.percent, -2)
                            + '%</div>';
                    },
                    background : {
                        opacity : 0.5,
                        color : '#000'
                    }
                }
            }
        },
        legend : {
            show : true
        }
    });

    // Creates APDEX table
    createTable($("#apdexTable"), {"supportsControllersDiscrimination": true, "overall": {"data": [0.8673389004559202, 500, 1500, "Total"], "isController": false}, "titles": ["Apdex", "T (Toleration threshold)", "F (Frustration threshold)", "Label"], "items": [{"data": [0.8742984380203865, 500, 1500, "ACME - uriinfo - URL not in 40M w/ 2 paths"], "isController": false}, {"data": [0.8713502050436129, 500, 1500, "ACME - uriinfo - URL in 40M w/ 5 paths & querystring"], "isController": false}, {"data": [0.8510671227815175, 500, 1500, "ACME - uriinfo - URL in 40M w/ 1 path & query string"], "isController": false}, {"data": [0.873049149435469, 500, 1500, "ACME - uriinfo - URL not in 40M w/ 5 paths"], "isController": false}]}, function(index, item){
        switch(index){
            case 0:
                item = item.toFixed(3);
                break;
            case 1:
            case 2:
                item = formatDuration(item);
                break;
        }
        return item;
    }, [[0, 0]], 3);

    // Create statistics table
    createTable($("#statisticsTable"), {"supportsControllersDiscrimination": true, "overall": {"data": ["Total", 9633702, 91677, 0.9516279411590685, 896.4006871916287, 0, 139022, 9406.0, 21004.0, 21033.0, 2666.2838066544023, 2025.8550093471754, 1297.4605798660803], "isController": false}, "titles": ["Label", "#Samples", "KO", "Error %", "Average", "Min", "Max", "90th pct", "95th pct", "99th pct", "Throughput", "Received", "Sent"], "items": [{"data": ["ACME - uriinfo - URL not in 40M w/ 2 paths", 2393053, 17165, 0.7172845733044776, 821.7501785376544, 0, 139022, 2366.600000000006, 9013.95, 21014.0, 662.3763088769745, 449.3360805989522, 310.27806088820955], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 5 paths & querystring", 2413389, 19694, 0.8160309009446881, 847.3674036800531, 0, 139015, 3006.0, 9044.95, 21021.0, 668.0018090990819, 575.9015215022193, 343.039574275959], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 1 path & query string", 2452014, 37937, 1.547177136835271, 1078.0311238026309, 0, 139022, 3166.9000000000015, 9046.0, 21018.99, 678.6347783946283, 460.4829546398067, 312.65473613172634], "isController": false}, {"data": ["ACME - uriinfo - URL not in 40M w/ 5 paths", 2375246, 16881, 0.710705333258113, 833.9307945366103, 0, 139021, 3011.0, 9040.95, 21019.0, 657.4596783680524, 540.2832859071436, 331.58178418213083], "isController": false}]}, function(index, item){
        switch(index){
            // Errors pct
            case 3:
                item = item.toFixed(2) + '%';
                break;
            // Mean
            case 4:
            // Mean
            case 7:
            // Percentile 1
            case 8:
            // Percentile 2
            case 9:
            // Percentile 3
            case 10:
            // Throughput
            case 11:
            // Kbytes/s
            case 12:
            // Sent Kbytes/s
                item = item.toFixed(2);
                break;
        }
        return item;
    }, [[0, 0]], 0, summaryTableHeader);

    // Create error table
    createTable($("#errorsTable"), {"supportsControllersDiscrimination": false, "titles": ["Type of error", "Number of errors", "% in errors", "% in all samples"], "items": [{"data": ["Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 269, 0.29342146885260206, 0.0027922806829607143], "isController": false}, {"data": ["Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 437, 0.4766735386192829, 0.004536158581612759], "isController": false}, {"data": ["Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 4997, 5.4506582894291915, 0.051869987259311114], "isController": false}, {"data": ["Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 85878, 93.67453123466082, 0.8914330129788113], "isController": false}, {"data": ["Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 96, 0.10471546843810334, 9.96501656372597E-4], "isController": false}]}, function(index, item){
        switch(index){
            case 2:
            case 3:
                item = item.toFixed(2) + '%';
                break;
        }
        return item;
    }, [[1, 1]]);

        // Create top5 errors by sampler
    createTable($("#top5ErrorsBySamplerTable"), {"supportsControllersDiscrimination": false, "overall": {"data": ["Total", 9633702, 91677, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 85878, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 4997, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 437, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 269, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 96], "isController": false}, "titles": ["Sample", "#Samples", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors"], "items": [{"data": ["ACME - uriinfo - URL not in 40M w/ 2 paths", 2393053, 17165, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 15640, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 1333, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 109, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 65, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 18], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 5 paths & querystring", 2413389, 19694, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 18290, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 1182, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 120, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 75, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 27], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 1 path & query string", 2452014, 37937, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 36575, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 1179, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 91, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 61, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 31], "isController": false}, {"data": ["ACME - uriinfo - URL not in 40M w/ 5 paths", 2375246, 16881, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 15373, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 1303, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 117, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 68, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 20], "isController": false}]}, function(index, item){
        return item;
    }, [[0, 0]], 0);

});
