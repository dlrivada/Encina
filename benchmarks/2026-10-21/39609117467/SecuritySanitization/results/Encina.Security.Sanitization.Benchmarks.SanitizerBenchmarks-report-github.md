```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  16,747.17 ns |    122.159 ns |  80.800 ns |  1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  22,143.86 ns |    236.216 ns | 156.242 ns |  1.322 |    0.01 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 186,498.16 ns |    890.682 ns | 530.030 ns | 11.136 |    0.06 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     207.77 ns |      1.462 ns |   0.967 ns |  0.012 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     315.96 ns |      0.858 ns |   0.449 ns |  0.019 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      28.46 ns |      0.336 ns |   0.200 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      28.25 ns |      0.335 ns |   0.221 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     452.23 ns |      1.958 ns |   1.165 ns |  0.027 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     289.25 ns |      9.403 ns |   6.220 ns |  0.017 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |               |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  15,905.87 ns |    928.269 ns |  50.882 ns |  1.000 |    0.00 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  22,294.01 ns |  3,975.407 ns | 217.905 ns |  1.402 |    0.01 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 187,555.16 ns | 16,039.999 ns | 879.206 ns | 11.792 |    0.06 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     202.70 ns |     34.557 ns |   1.894 ns |  0.013 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     308.33 ns |     19.092 ns |   1.047 ns |  0.019 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      28.14 ns |      2.619 ns |   0.144 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      28.29 ns |      2.759 ns |   0.151 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     457.01 ns |     12.563 ns |   0.689 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     281.36 ns |     73.681 ns |   4.039 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
