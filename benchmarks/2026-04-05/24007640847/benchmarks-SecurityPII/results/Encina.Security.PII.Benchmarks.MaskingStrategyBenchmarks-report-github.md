```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         99.18 ns | 2.320 ns | 1.535 ns |  1.00 |    0.02 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        433.68 ns | 5.063 ns | 3.349 ns |  4.37 |    0.07 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        489.02 ns | 7.664 ns | 4.561 ns |  4.93 |    0.08 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        410.30 ns | 3.397 ns | 2.247 ns |  4.14 |    0.06 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        179.89 ns | 1.776 ns | 1.057 ns |  1.81 |    0.03 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        121.34 ns | 2.727 ns | 1.804 ns |  1.22 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        222.17 ns | 1.597 ns | 1.056 ns |  2.24 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        177.74 ns | 0.707 ns | 0.468 ns |  1.79 |    0.03 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         53.49 ns | 0.704 ns | 0.368 ns |  0.54 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         69.46 ns | 0.524 ns | 0.347 ns |  0.70 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        105.07 ns | 2.397 ns | 1.585 ns |  1.06 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        596.86 ns | 5.532 ns | 3.659 ns |  6.02 |    0.09 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |              |             |                  |          |          |       |         |        |           |             |
| Email_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,802,020.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |      - |     224 B |        1.00 |
| Phone_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,863,955.00 ns |       NA | 0.000 ns |  1.28 |    0.00 |      - |     520 B |        2.32 |
| CreditCard_Partial  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,902,857.00 ns |       NA | 0.000 ns |  1.29 |    0.00 |      - |     544 B |        2.43 |
| SSN_Partial         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,920,952.00 ns |       NA | 0.000 ns |  1.29 |    0.00 |      - |     520 B |        2.32 |
| Name_Partial        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,261,348.00 ns |       NA | 0.000 ns |  1.12 |    0.00 |      - |     280 B |        1.25 |
| Address_Partial     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,312,524.00 ns |       NA | 0.000 ns |  0.87 |    0.00 |      - |     328 B |        1.46 |
| DateOfBirth_Partial | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,369,160.00 ns |       NA | 0.000 ns |  1.41 |    0.00 |      - |     384 B |        1.71 |
| IPAddress_Partial   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,519,030.00 ns |       NA | 0.000 ns |  1.19 |    0.00 |      - |     264 B |        1.18 |
| Custom_FullMasking  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,684,630.00 ns |       NA | 0.000 ns |  0.97 |    0.00 |      - |     128 B |        0.57 |
| Email_Short         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,795,057.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |      - |     128 B |        0.57 |
| Email_Long          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,846,283.00 ns |       NA | 0.000 ns |  1.01 |    0.00 |      - |     328 B |        1.46 |
| RegexPattern        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,416,395.00 ns |       NA | 0.000 ns |  4.32 |    0.00 |      - |     416 B |        1.86 |
