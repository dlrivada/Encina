```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 100.67 ns |  3.361 ns | 2.223 ns |  1.00 |    0.03 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 422.07 ns |  3.621 ns | 2.395 ns |  4.19 |    0.09 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 532.66 ns |  5.709 ns | 3.398 ns |  5.29 |    0.12 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 413.74 ns |  7.757 ns | 4.616 ns |  4.11 |    0.10 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 186.45 ns |  2.900 ns | 1.918 ns |  1.85 |    0.04 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 112.06 ns |  1.599 ns | 1.058 ns |  1.11 |    0.03 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 244.77 ns |  6.005 ns | 3.573 ns |  2.43 |    0.06 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 184.66 ns |  3.700 ns | 2.447 ns |  1.84 |    0.04 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  65.06 ns |  0.494 ns | 0.294 ns |  0.65 |    0.01 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  73.71 ns |  0.330 ns | 0.197 ns |  0.73 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 107.04 ns |  1.241 ns | 0.821 ns |  1.06 |    0.02 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 651.41 ns |  1.981 ns | 1.310 ns |  6.47 |    0.14 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           |  98.98 ns |  9.515 ns | 0.522 ns |  1.00 |    0.01 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 435.09 ns | 58.823 ns | 3.224 ns |  4.40 |    0.03 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 520.70 ns | 81.248 ns | 4.453 ns |  5.26 |    0.05 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 423.53 ns | 61.023 ns | 3.345 ns |  4.28 |    0.04 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 179.88 ns | 12.549 ns | 0.688 ns |  1.82 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 114.15 ns | 12.467 ns | 0.683 ns |  1.15 |    0.01 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 234.83 ns | 72.138 ns | 3.954 ns |  2.37 |    0.04 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 185.33 ns |  3.449 ns | 0.189 ns |  1.87 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  66.00 ns |  3.958 ns | 0.217 ns |  0.67 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  73.99 ns |  1.458 ns | 0.080 ns |  0.75 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 106.92 ns | 10.645 ns | 0.584 ns |  1.08 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 639.30 ns | 14.840 ns | 0.813 ns |  6.46 |    0.03 | 0.0229 |     392 B |        1.75 |
