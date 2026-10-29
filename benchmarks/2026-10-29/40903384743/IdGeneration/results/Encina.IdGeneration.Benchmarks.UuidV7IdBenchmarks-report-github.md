```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 676.0 ns |  25.34 ns | 16.76 ns |  1.02 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 431.2 ns |   0.87 ns |  0.45 ns |  0.65 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 664.1 ns |   6.65 ns |  4.40 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 672.7 ns | 139.36 ns |  7.64 ns |  1.01 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 434.3 ns |  45.03 ns |  2.47 ns |  0.65 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 666.9 ns |  77.81 ns |  4.27 ns |  1.00 |    0.01 |         - |          NA |
