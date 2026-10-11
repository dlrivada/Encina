```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method             | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error    | StdDev   | Median   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------- |----------- |--------------- |------------ |------------ |---------:|---------:|---------:|---------:|------:|--------:|----------:|------------:|
| Generate_GetValue  | Job-YFEFPZ | 10             | Default     | 3           | 974.0 ns |  2.48 ns |  1.30 ns | 974.4 ns |  1.06 |    0.01 |         - |          NA |
| NewGuid_Comparison | Job-YFEFPZ | 10             | Default     | 3           | 566.4 ns | 12.45 ns |  8.24 ns | 563.5 ns |  0.61 |    0.01 |         - |          NA |
| Generate           | Job-YFEFPZ | 10             | Default     | 3           | 921.1 ns |  9.08 ns |  5.40 ns | 919.5 ns |  1.00 |    0.01 |         - |          NA |
|                    |            |                |             |             |          |          |          |          |       |         |           |             |
| Generate_GetValue  | MediumRun  | 15             | 2           | 10          | 924.3 ns | 23.59 ns | 33.84 ns | 909.4 ns |  0.98 |    0.05 |         - |          NA |
| NewGuid_Comparison | MediumRun  | 15             | 2           | 10          | 559.0 ns |  1.24 ns |  1.70 ns | 558.5 ns |  0.60 |    0.02 |         - |          NA |
| Generate           | MediumRun  | 15             | 2           | 10          | 939.8 ns | 25.19 ns | 34.48 ns | 967.2 ns |  1.00 |    0.05 |         - |          NA |
