```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         95.50 ns | 1.256 ns | 0.831 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        415.32 ns | 2.575 ns | 1.703 ns |  4.35 |    0.04 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        487.48 ns | 5.368 ns | 3.551 ns |  5.10 |    0.06 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        398.54 ns | 1.877 ns | 1.241 ns |  4.17 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        178.71 ns | 2.298 ns | 1.520 ns |  1.87 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        118.53 ns | 0.738 ns | 0.439 ns |  1.24 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        218.21 ns | 2.177 ns | 1.440 ns |  2.29 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        177.05 ns | 1.325 ns | 0.789 ns |  1.85 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         53.11 ns | 0.509 ns | 0.336 ns |  0.56 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         68.66 ns | 0.434 ns | 0.287 ns |  0.72 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        101.27 ns | 0.827 ns | 0.547 ns |  1.06 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        605.93 ns | 1.625 ns | 0.967 ns |  6.35 |    0.05 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |              |             |                  |          |          |       |         |        |           |             |
| Email_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,717,814.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |      - |     224 B |        1.00 |
| Phone_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,038,775.00 ns |       NA | 0.000 ns |  1.36 |    0.00 |      - |     520 B |        2.32 |
| CreditCard_Partial  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,811,890.00 ns |       NA | 0.000 ns |  1.29 |    0.00 |      - |     544 B |        2.43 |
| SSN_Partial         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,824,424.00 ns |       NA | 0.000 ns |  1.30 |    0.00 |      - |     520 B |        2.32 |
| Name_Partial        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,185,128.00 ns |       NA | 0.000 ns |  1.13 |    0.00 |      - |     280 B |        1.25 |
| Address_Partial     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,306,454.00 ns |       NA | 0.000 ns |  0.89 |    0.00 |      - |     328 B |        1.46 |
| DateOfBirth_Partial | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,292,950.00 ns |       NA | 0.000 ns |  1.42 |    0.00 |      - |     384 B |        1.71 |
| IPAddress_Partial   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,445,355.00 ns |       NA | 0.000 ns |  1.20 |    0.00 |      - |     264 B |        1.18 |
| Custom_FullMasking  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,775,993.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |      - |     128 B |        0.57 |
| Email_Short         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,678,731.00 ns |       NA | 0.000 ns |  0.99 |    0.00 |      - |     128 B |        0.57 |
| Email_Long          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,801,961.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |      - |     328 B |        1.46 |
| RegexPattern        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,488,335.00 ns |       NA | 0.000 ns |  4.17 |    0.00 |      - |     416 B |        1.86 |
