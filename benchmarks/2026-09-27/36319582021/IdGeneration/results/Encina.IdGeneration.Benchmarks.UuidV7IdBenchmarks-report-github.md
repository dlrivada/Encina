```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 872.4 ns |   7.05 ns |  3.69 ns |  0.99 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 557.5 ns |   0.86 ns |  0.51 ns |  0.63 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 882.6 ns |   1.13 ns |  0.67 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 956.3 ns | 743.51 ns | 40.75 ns |  1.09 |    0.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 559.9 ns |   1.34 ns |  0.07 ns |  0.64 |    0.00 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 880.9 ns |  37.26 ns |  2.04 ns |  1.00 |    0.00 |         - |          NA |
