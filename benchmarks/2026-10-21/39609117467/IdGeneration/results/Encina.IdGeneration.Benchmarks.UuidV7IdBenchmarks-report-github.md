```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.50GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 662.8 ns |   8.10 ns |  5.36 ns |  0.97 |    0.04 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 324.5 ns |   1.64 ns |  0.86 ns |  0.47 |    0.02 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 685.1 ns |  39.75 ns | 26.29 ns |  1.00 |    0.05 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 676.8 ns | 392.09 ns | 21.49 ns |  1.01 |    0.03 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 325.3 ns |   3.74 ns |  0.20 ns |  0.49 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 667.1 ns |  71.11 ns |  3.90 ns |  1.00 |    0.01 |         - |          NA |
