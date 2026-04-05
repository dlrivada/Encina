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
| Email_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        103.07 ns | 1.232 ns | 0.815 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        419.65 ns | 7.592 ns | 5.021 ns |  4.07 |    0.06 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        491.83 ns | 3.606 ns | 2.146 ns |  4.77 |    0.04 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        413.13 ns | 7.931 ns | 5.246 ns |  4.01 |    0.06 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        184.33 ns | 1.654 ns | 0.865 ns |  1.79 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        127.97 ns | 2.225 ns | 1.472 ns |  1.24 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        226.09 ns | 3.485 ns | 2.305 ns |  2.19 |    0.03 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        185.39 ns | 1.768 ns | 1.169 ns |  1.80 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         57.10 ns | 1.150 ns | 0.760 ns |  0.55 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         72.82 ns | 0.754 ns | 0.499 ns |  0.71 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        108.61 ns | 1.122 ns | 0.668 ns |  1.05 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        596.61 ns | 3.969 ns | 2.625 ns |  5.79 |    0.05 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |              |             |                  |          |          |       |         |        |           |             |
| Email_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,650,272.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |      - |     224 B |        1.00 |
| Phone_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,780,965.00 ns |       NA | 0.000 ns |  1.31 |    0.00 |      - |     520 B |        2.32 |
| CreditCard_Partial  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,934,942.00 ns |       NA | 0.000 ns |  1.35 |    0.00 |      - |     544 B |        2.43 |
| SSN_Partial         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,863,963.00 ns |       NA | 0.000 ns |  1.33 |    0.00 |      - |     520 B |        2.32 |
| Name_Partial        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,104,343.00 ns |       NA | 0.000 ns |  1.12 |    0.00 |      - |     280 B |        1.25 |
| Address_Partial     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,258,472.00 ns |       NA | 0.000 ns |  0.89 |    0.00 |      - |     328 B |        1.46 |
| DateOfBirth_Partial | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,396,008.00 ns |       NA | 0.000 ns |  1.48 |    0.00 |      - |     384 B |        1.71 |
| IPAddress_Partial   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,500,534.00 ns |       NA | 0.000 ns |  1.23 |    0.00 |      - |     264 B |        1.18 |
| Custom_FullMasking  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,612,814.00 ns |       NA | 0.000 ns |  0.99 |    0.00 |      - |     128 B |        0.57 |
| Email_Short         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,795,135.00 ns |       NA | 0.000 ns |  1.04 |    0.00 |      - |     128 B |        0.57 |
| Email_Long          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,948,192.00 ns |       NA | 0.000 ns |  1.08 |    0.00 |      - |     328 B |        1.46 |
| RegexPattern        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,934,135.00 ns |       NA | 0.000 ns |  6.01 |    0.00 |      - |     416 B |        1.86 |
