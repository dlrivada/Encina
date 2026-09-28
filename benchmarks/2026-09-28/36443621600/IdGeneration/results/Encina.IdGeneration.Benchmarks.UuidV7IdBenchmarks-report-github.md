```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method             | Job        | IterationCount | LaunchCount | Mean     | Error     | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |---------:|----------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 882.8 ns |   4.61 ns |  2.74 ns |  1.01 |    0.00 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 559.2 ns |   0.87 ns |  0.52 ns |  0.64 |    0.00 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 875.0 ns |   2.49 ns |  1.65 ns |  1.00 |    0.00 |         - |          NA |
|                    |            |                |             |          |           |          |       |         |           |             |
| Generate_GetValue  | ShortRun   | 3              | 1           | 905.8 ns | 572.38 ns | 31.37 ns |  0.96 |    0.04 |         - |          NA |
| NewGuid_Comparison | ShortRun   | 3              | 1           | 565.2 ns | 102.15 ns |  5.60 ns |  0.60 |    0.01 |         - |          NA |
| Generate           | ShortRun   | 3              | 1           | 946.6 ns | 440.46 ns | 24.14 ns |  1.00 |    0.03 |         - |          NA |
