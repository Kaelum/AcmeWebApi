These are the Jmeter 4.0 tests that we ran, but we can't provide our data files due to privacy
concerns.  The data files are just lists of URLs, 1 per line.  You can use any URLs that you want,
in fact for this test, it can be any text under 2048 characters per line.  The more the better, but
I think that only 1 is required.  The tests have 1 or 2 User Defined Variables defined as follows:

In 40M File:		C:\Users\Administrator\Desktop\Sample Data For Jmeter\in40.txt
Not in 40M File:	C:\Users\Administrator\Desktop\Sample Data For Jmeter\notin40.txt

You can change the value (filepath) to match your systems.  Here is a PowerShell command that I used
to run one of my tests:

jmeter -n -t 'ACME_Variable_UriInfo_Test.jmx' -Jhost='host machine IP or DNS name' -Jport=5000 -Jduration=3600 -Jthreads=3500 -Jthroughput=360000

The "host", "port", "duration", "threads", and "throughput" parameters are required.  Make sure that
you "Publish" the AcmeWebApi project in order to create the same builds that we use.