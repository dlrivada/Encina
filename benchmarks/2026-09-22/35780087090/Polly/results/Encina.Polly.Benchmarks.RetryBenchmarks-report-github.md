```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|--------:|------:|--------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 592.7 ns |  27.60 ns | 7.17 ns |  1.00 |    0.02 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 613.8 ns |  32.49 ns | 8.44 ns |  1.04 |    0.02 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |          |           |         |       |         |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 624.7 ns | 162.21 ns | 8.89 ns |  1.00 |    0.02 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 662.2 ns | 150.88 ns | 8.27 ns |  1.06 |    0.02 | 0.0525 |     880 B |        1.00 |
