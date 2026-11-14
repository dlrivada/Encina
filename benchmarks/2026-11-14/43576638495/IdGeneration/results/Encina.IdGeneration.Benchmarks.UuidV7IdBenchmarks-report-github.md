```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 654.9 ns |  10.97 ns |  7.25 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 690.2 ns |   5.50 ns |  3.64 ns |  1.06 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 653.2 ns |   4.28 ns |  2.83 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 654.1 ns |  18.02 ns |  0.99 ns |  0.99 |    0.02 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 711.6 ns | 210.31 ns | 11.53 ns |  1.08 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 661.5 ns | 221.40 ns | 12.14 ns |  1.00 |    0.02 |         - |          NA |
