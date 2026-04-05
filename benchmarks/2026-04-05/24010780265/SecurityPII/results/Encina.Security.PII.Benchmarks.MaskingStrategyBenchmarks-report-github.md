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
| Email_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         95.47 ns | 0.648 ns | 0.428 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        415.53 ns | 2.954 ns | 1.954 ns |  4.35 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        490.77 ns | 1.654 ns | 0.984 ns |  5.14 |    0.02 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        403.96 ns | 3.423 ns | 2.264 ns |  4.23 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        178.16 ns | 1.711 ns | 1.018 ns |  1.87 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        120.34 ns | 1.949 ns | 1.160 ns |  1.26 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        217.59 ns | 1.783 ns | 1.061 ns |  2.28 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        177.62 ns | 1.441 ns | 0.953 ns |  1.86 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         54.08 ns | 0.500 ns | 0.331 ns |  0.57 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         67.99 ns | 0.226 ns | 0.134 ns |  0.71 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        105.62 ns | 5.579 ns | 3.690 ns |  1.11 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        601.83 ns | 4.643 ns | 2.763 ns |  6.30 |    0.04 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |              |             |                  |          |          |       |         |        |           |             |
| Email_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,910,231.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |      - |     224 B |        1.00 |
| Phone_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,263,028.00 ns |       NA | 0.000 ns |  1.35 |    0.00 |      - |     520 B |        2.32 |
| CreditCard_Partial  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,836,521.00 ns |       NA | 0.000 ns |  1.24 |    0.00 |      - |     544 B |        2.43 |
| SSN_Partial         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,831,820.00 ns |       NA | 0.000 ns |  1.24 |    0.00 |      - |     520 B |        2.32 |
| Name_Partial        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,125,260.00 ns |       NA | 0.000 ns |  1.05 |    0.00 |      - |     280 B |        1.25 |
| Address_Partial     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,245,227.00 ns |       NA | 0.000 ns |  0.83 |    0.00 |      - |     328 B |        1.46 |
| DateOfBirth_Partial | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,294,173.00 ns |       NA | 0.000 ns |  1.35 |    0.00 |      - |     384 B |        1.71 |
| IPAddress_Partial   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,523,815.00 ns |       NA | 0.000 ns |  1.16 |    0.00 |      - |     264 B |        1.18 |
| Custom_FullMasking  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,692,472.00 ns |       NA | 0.000 ns |  0.94 |    0.00 |      - |     128 B |        0.57 |
| Email_Short         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,727,208.00 ns |       NA | 0.000 ns |  0.95 |    0.00 |      - |     128 B |        0.57 |
| Email_Long          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,730,243.00 ns |       NA | 0.000 ns |  0.95 |    0.00 |      - |     328 B |        1.46 |
| RegexPattern        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 15,198,101.00 ns |       NA | 0.000 ns |  3.89 |    0.00 |      - |     416 B |        1.86 |
