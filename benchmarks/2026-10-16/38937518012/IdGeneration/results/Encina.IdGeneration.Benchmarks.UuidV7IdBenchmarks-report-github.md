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
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 692.0 ns |  18.58 ns |  9.72 ns |  1.02 |    0.02 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 435.4 ns |  10.20 ns |  6.07 ns |  0.64 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 678.1 ns |  20.04 ns | 11.93 ns |  1.00 |    0.02 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 683.5 ns | 192.02 ns | 10.53 ns |  1.00 |    0.01 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 449.6 ns | 216.10 ns | 11.85 ns |  0.66 |    0.02 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 685.3 ns | 100.44 ns |  5.51 ns |  1.00 |    0.01 |         - |          NA |
