```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error    | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|---------:|--------:|------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 681.0 ns |  4.56 ns | 3.02 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 679.5 ns |  1.75 ns | 1.04 ns |  0.99 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 686.3 ns |  3.66 ns | 1.92 ns |  1.00 |         - |          NA |
|                    |            |                |             |          |          |         |       |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 655.5 ns | 54.36 ns | 2.98 ns |  0.99 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 681.1 ns | 24.04 ns | 1.32 ns |  1.03 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 659.3 ns | 31.30 ns | 1.72 ns |  1.00 |         - |          NA |
