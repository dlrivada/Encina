```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 865.6 ns |   9.73 ns |  1.51 ns |  1.00 |    0.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 830.6 ns |  27.80 ns |  4.30 ns |  0.96 |    0.00 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |          |           |          |       |         |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 828.8 ns | 210.24 ns | 11.52 ns |  1.00 |    0.02 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 812.8 ns |  89.73 ns |  4.92 ns |  0.98 |    0.01 | 0.0525 |     880 B |        1.00 |
