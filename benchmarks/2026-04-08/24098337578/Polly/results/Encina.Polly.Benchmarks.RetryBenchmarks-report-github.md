```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 3           | 771.6 ns |  5.68 ns |  1.47 ns |  1.00 |    0.00 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 3           | 765.9 ns | 15.66 ns |  4.07 ns |  0.99 |    0.01 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |             |          |          |          |       |         |        |           |             |
| NoRetryAttribute_Baseline          | MediumRun  | 15             | 2           | 10          | 771.8 ns |  9.88 ns | 14.48 ns |  1.00 |    0.03 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | MediumRun  | 15             | 2           | 10          | 781.3 ns |  3.09 ns |  4.53 ns |  1.01 |    0.02 | 0.0525 |     880 B |        1.00 |
