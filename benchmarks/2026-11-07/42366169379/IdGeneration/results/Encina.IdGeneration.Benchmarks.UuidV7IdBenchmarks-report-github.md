```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 509.2 ns | 12.29 ns | 7.32 ns |  1.02 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 427.8 ns |  5.83 ns | 3.86 ns |  0.86 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 499.8 ns | 12.28 ns | 8.12 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |          |         |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 473.4 ns | 29.74 ns | 1.63 ns |  0.99 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 437.5 ns | 44.96 ns | 2.46 ns |  0.92 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 476.3 ns | 60.49 ns | 3.32 ns |  1.00 |    0.01 |         - |          NA |
