```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |-------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  7,456.47 ns |    282.022 ns |   186.540 ns | 1.001 |    0.03 | 0.8621 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  9,448.91 ns |    204.827 ns |   121.890 ns | 1.268 |    0.03 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 71,622.32 ns |  2,208.954 ns | 1,461.086 ns | 9.611 |    0.29 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |    115.44 ns |      5.863 ns |     3.878 ns | 0.015 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |    181.86 ns |      6.699 ns |     3.987 ns | 0.024 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |     14.48 ns |      0.811 ns |     0.483 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |     14.41 ns |      0.497 ns |     0.329 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |    234.67 ns |      7.839 ns |     5.185 ns | 0.031 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |    147.49 ns |      3.755 ns |     2.484 ns | 0.020 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |              |               |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  7,417.24 ns |  2,309.802 ns |   126.608 ns | 1.000 |    0.02 | 0.8621 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  9,634.86 ns |  2,514.738 ns |   137.841 ns | 1.299 |    0.03 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 73,500.46 ns | 57,137.575 ns | 3,131.903 ns | 9.911 |    0.39 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |    111.10 ns |     20.899 ns |     1.146 ns | 0.015 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |    192.64 ns |    146.089 ns |     8.008 ns | 0.026 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |     14.36 ns |      5.655 ns |     0.310 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |     15.29 ns |      1.324 ns |     0.073 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |    228.91 ns |     20.337 ns |     1.115 ns | 0.031 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |    160.23 ns |     19.784 ns |     1.084 ns | 0.022 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
