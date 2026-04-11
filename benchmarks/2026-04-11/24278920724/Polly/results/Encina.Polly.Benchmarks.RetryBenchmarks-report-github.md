```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NTRUNJ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| NoRetryAttribute_Baseline          | Job-NTRUNJ | 5              | Default     | 3           | 813.6 ns | 32.08 ns |  4.96 ns | 813.9 ns |  1.00 |    0.01 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | Job-NTRUNJ | 5              | Default     | 3           | 769.3 ns | 16.50 ns |  4.29 ns | 771.6 ns |  0.95 |    0.01 | 0.0525 |     880 B |        1.00 |
|                                    |            |                |             |             |          |          |          |          |       |         |        |           |             |
| NoRetryAttribute_Baseline          | MediumRun  | 15             | 2           | 10          | 777.1 ns |  4.31 ns |  6.32 ns | 778.4 ns |  1.00 |    0.01 | 0.0525 |     880 B |        1.00 |
| WithRetryAttribute_NoActualRetries | MediumRun  | 15             | 2           | 10          | 814.6 ns | 13.46 ns | 19.73 ns | 804.0 ns |  1.05 |    0.03 | 0.0525 |     880 B |        1.00 |
