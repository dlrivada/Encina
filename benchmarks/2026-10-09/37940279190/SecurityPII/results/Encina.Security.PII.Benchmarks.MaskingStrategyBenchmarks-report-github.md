```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method              | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|-------------------- |----------- |--------------- |------------ |----------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| Email_Partial       | Job-YFEFPZ | 10             | Default     | 109.49 ns |  6.551 ns | 3.899 ns |  1.00 |    0.05 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | Job-YFEFPZ | 10             | Default     | 443.00 ns |  2.321 ns | 1.535 ns |  4.05 |    0.14 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | Job-YFEFPZ | 10             | Default     | 525.34 ns |  2.890 ns | 1.720 ns |  4.80 |    0.17 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | Job-YFEFPZ | 10             | Default     | 449.49 ns | 10.383 ns | 6.868 ns |  4.11 |    0.16 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | Job-YFEFPZ | 10             | Default     | 199.29 ns |  0.683 ns | 0.407 ns |  1.82 |    0.06 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | Job-YFEFPZ | 10             | Default     | 139.92 ns |  0.723 ns | 0.431 ns |  1.28 |    0.05 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | Job-YFEFPZ | 10             | Default     | 244.42 ns |  2.739 ns | 1.812 ns |  2.24 |    0.08 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | Job-YFEFPZ | 10             | Default     | 197.85 ns |  0.778 ns | 0.463 ns |  1.81 |    0.06 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | Job-YFEFPZ | 10             | Default     |  66.42 ns |  0.322 ns | 0.213 ns |  0.61 |    0.02 | 0.0076 |     128 B |        0.57 |
| Email_Short         | Job-YFEFPZ | 10             | Default     |  82.33 ns |  2.062 ns | 1.079 ns |  0.75 |    0.03 | 0.0076 |     128 B |        0.57 |
| Email_Long          | Job-YFEFPZ | 10             | Default     | 123.42 ns |  0.747 ns | 0.494 ns |  1.13 |    0.04 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | Job-YFEFPZ | 10             | Default     | 644.86 ns |  1.497 ns | 0.783 ns |  5.90 |    0.21 | 0.0229 |     392 B |        1.75 |
|                     |            |                |             |           |           |          |       |         |        |           |             |
| Email_Partial       | ShortRun   | 3              | 1           | 110.87 ns |  6.230 ns | 0.341 ns |  1.00 |    0.00 | 0.0134 |     224 B |        1.00 |
| Phone_Partial       | ShortRun   | 3              | 1           | 434.51 ns |  8.121 ns | 0.445 ns |  3.92 |    0.01 | 0.0310 |     520 B |        2.32 |
| CreditCard_Partial  | ShortRun   | 3              | 1           | 513.85 ns | 44.400 ns | 2.434 ns |  4.63 |    0.02 | 0.0324 |     544 B |        2.43 |
| SSN_Partial         | ShortRun   | 3              | 1           | 422.37 ns |  2.497 ns | 0.137 ns |  3.81 |    0.01 | 0.0310 |     520 B |        2.32 |
| Name_Partial        | ShortRun   | 3              | 1           | 196.72 ns | 12.756 ns | 0.699 ns |  1.77 |    0.01 | 0.0167 |     280 B |        1.25 |
| Address_Partial     | ShortRun   | 3              | 1           | 136.73 ns |  5.883 ns | 0.322 ns |  1.23 |    0.00 | 0.0196 |     328 B |        1.46 |
| DateOfBirth_Partial | ShortRun   | 3              | 1           | 242.50 ns |  8.048 ns | 0.441 ns |  2.19 |    0.01 | 0.0229 |     384 B |        1.71 |
| IPAddress_Partial   | ShortRun   | 3              | 1           | 195.90 ns |  8.885 ns | 0.487 ns |  1.77 |    0.01 | 0.0157 |     264 B |        1.18 |
| Custom_FullMasking  | ShortRun   | 3              | 1           |  64.66 ns |  4.496 ns | 0.246 ns |  0.58 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Short         | ShortRun   | 3              | 1           |  79.71 ns |  9.791 ns | 0.537 ns |  0.72 |    0.00 | 0.0076 |     128 B |        0.57 |
| Email_Long          | ShortRun   | 3              | 1           | 118.67 ns | 13.927 ns | 0.763 ns |  1.07 |    0.01 | 0.0196 |     328 B |        1.46 |
| RegexPattern        | ShortRun   | 3              | 1           | 653.56 ns | 13.248 ns | 0.726 ns |  5.90 |    0.02 | 0.0229 |     392 B |        1.75 |
