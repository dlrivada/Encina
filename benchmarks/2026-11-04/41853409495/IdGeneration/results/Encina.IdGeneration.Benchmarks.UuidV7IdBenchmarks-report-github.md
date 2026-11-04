```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 528.1 ns |  13.03 ns |  7.76 ns |  1.01 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 450.4 ns |   7.49 ns |  4.96 ns |  0.86 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 523.1 ns |  11.13 ns |  6.62 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 511.0 ns | 208.20 ns | 11.41 ns |  0.99 |    0.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 438.8 ns | 118.86 ns |  6.52 ns |  0.85 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 518.9 ns | 257.14 ns | 14.09 ns |  1.00 |    0.03 |         - |          NA |
