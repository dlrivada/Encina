```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 703.8 ns |  25.35 ns | 16.77 ns |  0.99 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 427.6 ns |  12.01 ns |  7.94 ns |  0.60 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 708.2 ns |  10.62 ns |  6.32 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 682.9 ns | 136.03 ns |  7.46 ns |  0.98 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 434.0 ns | 105.98 ns |  5.81 ns |  0.62 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 696.9 ns | 168.84 ns |  9.25 ns |  1.00 |    0.02 |         - |          NA |
