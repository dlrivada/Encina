```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|---------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 104.24 ns | 0.888 ns |  0.528 ns | 104.32 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 430.24 ns | 1.377 ns |  0.819 ns | 429.91 ns |  4.13 |    0.02 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 494.72 ns | 1.804 ns |  1.193 ns | 494.38 ns |  4.75 |    0.03 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 409.41 ns | 3.347 ns |  1.992 ns | 409.16 ns |  3.93 |    0.03 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 184.13 ns | 1.259 ns |  0.749 ns | 184.25 ns |  1.77 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           | 118.38 ns | 2.355 ns |  1.232 ns | 118.54 ns |  1.14 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 218.75 ns | 3.510 ns |  2.322 ns | 217.74 ns |  2.10 |    0.02 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           | 180.43 ns | 3.527 ns |  2.333 ns | 181.09 ns |  1.73 |    0.02 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  57.37 ns | 1.244 ns |  0.823 ns |  57.59 ns |  0.55 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  71.07 ns | 1.869 ns |  1.236 ns |  71.35 ns |  0.68 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           | 105.94 ns | 1.223 ns |  0.809 ns | 106.19 ns |  1.02 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 615.67 ns | 4.945 ns |  2.943 ns | 614.99 ns |  5.91 |    0.04 | 0.0248 |     416 B |        1.86 |
|                     |            |                |             |             |           |          |           |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  99.79 ns | 1.875 ns |  2.807 ns |  99.31 ns |  1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 430.26 ns | 2.057 ns |  3.016 ns | 430.41 ns |  4.31 |    0.12 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 507.51 ns | 7.297 ns | 10.696 ns | 502.97 ns |  5.09 |    0.17 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 411.94 ns | 5.683 ns |  7.966 ns | 416.56 ns |  4.13 |    0.14 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 190.46 ns | 0.903 ns |  1.324 ns | 190.46 ns |  1.91 |    0.05 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          | 129.51 ns | 1.135 ns |  1.699 ns | 129.35 ns |  1.30 |    0.04 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 230.42 ns | 2.028 ns |  2.972 ns | 230.21 ns |  2.31 |    0.07 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          | 181.28 ns | 1.739 ns |  2.603 ns | 181.19 ns |  1.82 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  56.02 ns | 0.820 ns |  1.227 ns |  55.81 ns |  0.56 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  71.29 ns | 0.670 ns |  1.003 ns |  71.34 ns |  0.71 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          | 108.89 ns | 0.846 ns |  1.266 ns | 109.00 ns |  1.09 |    0.03 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 611.54 ns | 8.823 ns | 12.369 ns | 602.77 ns |  6.13 |    0.21 | 0.0248 |     416 B |        1.86 |
