```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 613.7 ns |   3.77 ns |  2.24 ns |  1.00 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 393.8 ns |   1.64 ns |  1.08 ns |  0.64 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 615.6 ns |   2.83 ns |  1.87 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 619.0 ns | 134.28 ns |  7.36 ns |  0.99 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 393.9 ns |  17.31 ns |  0.95 ns |  0.63 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 624.6 ns | 199.63 ns | 10.94 ns |  1.00 |    0.02 |         - |          NA |
