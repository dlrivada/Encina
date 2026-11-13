```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev       | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  15,014.50 ns |    241.000 ns |   159.407 ns |  1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  21,921.83 ns |    356.028 ns |   235.490 ns |  1.460 |    0.02 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 186,555.68 ns |  3,341.021 ns | 1,747.419 ns | 12.426 |    0.17 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     210.79 ns |      0.990 ns |     0.589 ns |  0.014 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     311.84 ns |      3.228 ns |     2.135 ns |  0.021 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      27.80 ns |      0.436 ns |     0.259 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      27.75 ns |      0.450 ns |     0.268 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     452.74 ns |      4.139 ns |     2.738 ns |  0.030 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     291.75 ns |      8.316 ns |     5.500 ns |  0.019 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |               |              |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  15,973.99 ns |  4,480.450 ns |   245.589 ns |  1.000 |    0.02 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  21,977.50 ns |    436.967 ns |    23.952 ns |  1.376 |    0.02 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 189,867.46 ns | 30,633.294 ns | 1,679.114 ns | 11.888 |    0.18 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     202.99 ns |      9.377 ns |     0.514 ns |  0.013 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     311.58 ns |     15.336 ns |     0.841 ns |  0.020 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      29.39 ns |      0.880 ns |     0.048 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      29.56 ns |      3.512 ns |     0.192 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     501.71 ns |     56.335 ns |     3.088 ns |  0.031 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     298.60 ns |     20.123 ns |     1.103 ns |  0.019 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
