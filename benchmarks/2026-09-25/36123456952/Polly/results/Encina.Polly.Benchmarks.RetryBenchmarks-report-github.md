```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |---------:|----------:|----------:|------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 2.159 μs | 0.0234 μs | 0.0061 μs |  1.00 | 0.0763 |    1.3 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 2.202 μs | 0.0190 μs | 0.0049 μs |  1.02 | 0.0763 |    1.3 KB |        1.00 |
|                                    |            |                |             |          |           |           |       |        |           |             |
| NoRetryAttribute_Baseline          | ShortRun   | 3              | 1           | 2.235 μs | 0.0409 μs | 0.0022 μs |  1.00 | 0.0763 |    1.3 KB |        1.00 |
| WithRetryAttribute_NoActualRetries | ShortRun   | 3              | 1           | 2.144 μs | 0.0703 μs | 0.0039 μs |  0.96 | 0.0763 |    1.3 KB |        1.00 |
