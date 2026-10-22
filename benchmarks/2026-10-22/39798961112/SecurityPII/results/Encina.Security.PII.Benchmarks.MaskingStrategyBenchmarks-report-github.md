```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method              | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error    | StdDev   | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |------------ |----------:|---------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 3           |  58.60 ns | 0.607 ns | 0.361 ns |  58.73 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 3           | 238.87 ns | 7.955 ns | 5.262 ns | 238.96 ns |  4.08 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 3           | 296.62 ns | 9.105 ns | 5.418 ns | 295.03 ns |  5.06 |    0.09 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 3           | 236.42 ns | 7.632 ns | 4.542 ns | 235.15 ns |  4.03 |    0.08 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 3           | 105.08 ns | 3.777 ns | 2.498 ns | 105.39 ns |  1.79 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 3           |  65.04 ns | 1.579 ns | 1.044 ns |  65.27 ns |  1.11 |    0.02 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 3           | 145.24 ns | 3.828 ns | 2.532 ns | 145.80 ns |  2.48 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 3           |  98.46 ns | 1.046 ns | 0.692 ns |  98.40 ns |  1.68 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     | 3           |  38.07 ns | 1.853 ns | 1.225 ns |  37.79 ns |  0.65 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     | 3           |  41.11 ns | 1.137 ns | 0.676 ns |  41.03 ns |  0.70 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 3           |  61.52 ns | 2.082 ns | 1.089 ns |  61.40 ns |  1.05 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 3           | 359.74 ns | 5.372 ns | 3.197 ns | 360.14 ns |  6.14 |    0.06 | 0.0234 |     392 B |        1.75 |
|                     |            |                |             |             |           |          |          |           |       |         |        |           |             |
| Email_Partial       | MediumRun  | 15             | 2           | 10          |  57.28 ns | 1.125 ns | 1.613 ns |  57.59 ns |  1.00 |    0.04 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | MediumRun  | 15             | 2           | 10          | 247.68 ns | 2.986 ns | 4.376 ns | 246.86 ns |  4.33 |    0.14 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | MediumRun  | 15             | 2           | 10          | 289.57 ns | 4.368 ns | 6.265 ns | 289.88 ns |  5.06 |    0.18 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | MediumRun  | 15             | 2           | 10          | 239.08 ns | 4.346 ns | 6.093 ns | 238.46 ns |  4.18 |    0.16 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | MediumRun  | 15             | 2           | 10          | 105.14 ns | 1.887 ns | 2.767 ns | 105.21 ns |  1.84 |    0.07 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | MediumRun  | 15             | 2           | 10          |  61.33 ns | 1.242 ns | 1.700 ns |  61.29 ns |  1.07 |    0.04 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | MediumRun  | 15             | 2           | 10          | 131.24 ns | 2.122 ns | 3.111 ns | 130.95 ns |  2.29 |    0.08 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | MediumRun  | 15             | 2           | 10          |  97.52 ns | 1.278 ns | 1.874 ns |  97.12 ns |  1.70 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | MediumRun  | 15             | 2           | 10          |  38.53 ns | 0.609 ns | 0.893 ns |  38.40 ns |  0.67 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | MediumRun  | 15             | 2           | 10          |  48.94 ns | 5.522 ns | 7.920 ns |  42.90 ns |  0.86 |    0.14 | 0.0076 |     128 B |        0.57 |
| Email_Long          | MediumRun  | 15             | 2           | 10          |  62.28 ns | 1.182 ns | 1.695 ns |  62.49 ns |  1.09 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | MediumRun  | 15             | 2           | 10          | 370.55 ns | 4.964 ns | 7.277 ns | 370.82 ns |  6.47 |    0.22 | 0.0234 |     392 B |        1.75 |
