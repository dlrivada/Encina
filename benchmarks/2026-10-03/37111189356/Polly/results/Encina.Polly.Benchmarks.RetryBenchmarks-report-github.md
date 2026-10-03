```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.70GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 1.815 μs | 0.0495 μs | 0.0129 μs |  1.00 | 0.0515 |    1.3 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 1.752 μs | 0.0267 μs | 0.0069 μs |  0.97 | 0.0515 |    1.3 KB |        1.00 |
|                                    |            |                |             |          |           |           |       |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 1.809 μs | 0.1330 μs | 0.0073 μs |  1.00 | 0.0515 |    1.3 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 1.749 μs | 0.0778 μs | 0.0043 μs |  0.97 | 0.0515 |    1.3 KB |        1.00 |
