```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.55GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 749.0 ns |  22.63 ns | 14.97 ns |  0.98 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 364.0 ns |   8.46 ns |  5.60 ns |  0.47 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 766.7 ns |   6.32 ns |  3.76 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 768.0 ns | 201.58 ns | 11.05 ns |  0.99 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 374.9 ns |  12.69 ns |  0.70 ns |  0.48 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 774.5 ns |  97.57 ns |  5.35 ns |  1.00 |    0.01 |         - |          NA |
