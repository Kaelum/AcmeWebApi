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

    var data = {"OkPercent": 99.05334749397493, "KoPercent": 0.9466525060250807};
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
    createTable($("#apdexTable"), {"supportsControllersDiscrimination": true, "overall": {"data": [0.8442933843169566, 500, 1500, "Total"], "isController": false}, "titles": ["Apdex", "T (Toleration threshold)", "F (Frustration threshold)", "Label"], "items": [{"data": [0.8495751252331872, 500, 1500, "ACME - uriinfo - URL in 40M w/ 5 paths & querystring"], "isController": false}, {"data": [0.8282887550448228, 500, 1500, "ACME - uriinfo - URL in 40M w/ 1 path & query string"], "isController": false}, {"data": [0.8516623502410882, 500, 1500, "ACME - uriinfo - URL not in 40M w/ 2 paths"], "isController": false}, {"data": [0.8480397627566286, 500, 1500, "ACME - uriinfo - URL not in 40M w/ 5 paths"], "isController": false}]}, function(index, item){
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
    createTable($("#statisticsTable"), {"supportsControllersDiscrimination": true, "overall": {"data": ["Total", 8029768, 76014, 0.9466525060250807, 1149.4305169213605, 0, 283595, 12657.0, 14459.0, 21047.0, 2219.7278971741466, 1686.123264990559, 1080.141188893348], "isController": false}, "titles": ["Label", "#Samples", "KO", "Error %", "Average", "Min", "Max", "90th pct", "95th pct", "99th pct", "Throughput", "Received", "Sent"], "items": [{"data": ["ACME - uriinfo - URL in 40M w/ 5 paths & querystring", 2009651, 14195, 0.7063415488559954, 1066.7741976093962, 0, 283570, 3418.0, 11079.95, 21025.0, 555.6209575702816, 478.26997711994113, 285.66014379344483], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 1 path & query string", 2045166, 34803, 1.7017200559758963, 1377.9215916947408, 0, 283565, 3462.0, 11355.750000000004, 21046.0, 565.3606099759664, 385.023063376376, 260.07543721746356], "isController": false}, {"data": ["ACME - uriinfo - URL not in 40M w/ 2 paths", 1994706, 13787, 0.6911795522748716, 1063.963571072603, 0, 283595, 3226.0, 10848.0, 21023.0, 551.4943552661373, 373.66032389024434, 258.34519730717733], "isController": false}, {"data": ["ACME - uriinfo - URL not in 40M w/ 5 paths", 1980245, 13229, 0.6680486505457658, 1083.423334486289, 0, 283562, 3247.0, 10854.0, 21018.0, 547.500733366677, 449.36526376463934, 276.18364926982326], "isController": false}]}, function(index, item){
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
    createTable($("#errorsTable"), {"supportsControllersDiscrimination": false, "titles": ["Type of error", "Number of errors", "% in errors", "% in all samples"], "items": [{"data": ["Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 603, 0.7932749230404925, 0.007509556938631353], "isController": false}, {"data": ["Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 31, 0.04078196121767043, 3.860634578732536E-4], "isController": false}, {"data": ["Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 1980, 2.604783329386692, 0.02465824666416265], "isController": false}, {"data": ["Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 73294, 96.42171178993344, 0.9127785510116855], "isController": false}, {"data": ["Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 106, 0.1394479964217118, 0.0013200879527278996], "isController": false}]}, function(index, item){
        switch(index){
            case 2:
            case 3:
                item = item.toFixed(2) + '%';
                break;
        }
        return item;
    }, [[1, 1]]);

        // Create top5 errors by sampler
    createTable($("#top5ErrorsBySamplerTable"), {"supportsControllersDiscrimination": false, "overall": {"data": ["Total", 8029768, 76014, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 73294, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 1980, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 603, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 106, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 31], "isController": false}, "titles": ["Sample", "#Samples", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors", "Error", "#Errors"], "items": [{"data": ["ACME - uriinfo - URL in 40M w/ 5 paths & querystring", 2009651, 14195, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 13546, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 487, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 137, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 20, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 5], "isController": false}, {"data": ["ACME - uriinfo - URL in 40M w/ 1 path & query string", 2045166, 34803, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 34067, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 502, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 197, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 29, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 8], "isController": false}, {"data": ["ACME - uriinfo - URL not in 40M w/ 2 paths", 1994706, 13787, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 13153, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 481, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 117, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 29, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 7], "isController": false}, {"data": ["ACME - uriinfo - URL not in 40M w/ 5 paths", 1980245, 13229, "Non HTTP response code: java.net.ConnectException/Non HTTP response message: Connection timed out: connect", 12528, "Non HTTP response code: org.apache.http.NoHttpResponseException/Non HTTP response message: 34.221.10.152:5000 failed to respond", 510, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Connection reset", 152, "Non HTTP response code: java.net.SocketTimeoutException/Non HTTP response message: Read timed out", 28, "Non HTTP response code: java.net.SocketException/Non HTTP response message: Unrecognized Windows Sockets error: 0: recv failed", 11], "isController": false}]}, function(index, item){
        return item;
    }, [[0, 0]], 0);

});
