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
| Email_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        100.99 ns | 1.193 ns | 0.789 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        427.45 ns | 5.308 ns | 3.511 ns |  4.23 |    0.05 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        491.10 ns | 7.645 ns | 5.057 ns |  4.86 |    0.06 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        408.10 ns | 8.582 ns | 5.676 ns |  4.04 |    0.06 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        181.79 ns | 1.443 ns | 0.955 ns |  1.80 |    0.02 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        124.28 ns | 4.088 ns | 2.704 ns |  1.23 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        220.50 ns | 2.023 ns | 1.338 ns |  2.18 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        180.73 ns | 1.905 ns | 1.133 ns |  1.79 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         56.93 ns | 1.162 ns | 0.768 ns |  0.56 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         78.57 ns | 3.067 ns | 2.029 ns |  0.78 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        105.19 ns | 3.481 ns | 2.303 ns |  1.04 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        667.11 ns | 1.464 ns | 0.766 ns |  6.61 |    0.05 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |              |             |                  |          |          |       |         |        |           |             |
| Email_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,826,170.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |      - |     224 B |        1.00 |
| Phone_Partial       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,989,885.00 ns |       NA | 0.000 ns |  1.30 |    0.00 |      - |     520 B |        2.32 |
| CreditCard_Partial  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,907,672.00 ns |       NA | 0.000 ns |  1.28 |    0.00 |      - |     544 B |        2.43 |
| SSN_Partial         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,106,301.00 ns |       NA | 0.000 ns |  1.33 |    0.00 |      - |     520 B |        2.32 |
| Name_Partial        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,283,540.00 ns |       NA | 0.000 ns |  1.12 |    0.00 |      - |     280 B |        1.25 |
| Address_Partial     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,311,612.00 ns |       NA | 0.000 ns |  0.87 |    0.00 |      - |     328 B |        1.46 |
| DateOfBirth_Partial | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  5,398,465.00 ns |       NA | 0.000 ns |  1.41 |    0.00 |      - |     384 B |        1.71 |
| IPAddress_Partial   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,535,499.00 ns |       NA | 0.000 ns |  1.19 |    0.00 |      - |     264 B |        1.18 |
| Custom_FullMasking  | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,692,681.00 ns |       NA | 0.000 ns |  0.97 |    0.00 |      - |     128 B |        0.57 |
| Email_Short         | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,763,633.00 ns |       NA | 0.000 ns |  0.98 |    0.00 |      - |     128 B |        0.57 |
| Email_Long          | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,730,200.00 ns |       NA | 0.000 ns |  0.97 |    0.00 |      - |     328 B |        1.46 |
| RegexPattern        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 16,124,183.00 ns |       NA | 0.000 ns |  4.21 |    0.00 |      - |     416 B |        1.86 |
