```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 683.3 ns |   6.87 ns |  4.09 ns |  1.04 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 683.1 ns |   4.19 ns |  2.49 ns |  1.04 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 654.6 ns |   2.74 ns |  1.43 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 690.5 ns | 192.39 ns | 10.55 ns |  1.06 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 685.0 ns |   5.24 ns |  0.29 ns |  1.05 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 654.3 ns |  31.18 ns |  1.71 ns |  1.00 |         - |          NA |
