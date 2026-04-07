```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate             | Job-YFEFPZ | 10             | Default     | 242.1 ns | 0.11 ns | 0.06 ns |  1.00 |         - |          NA |
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 242.1 ns | 0.10 ns | 0.06 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 240.4 ns | 0.09 ns | 0.05 ns |  0.99 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 242.1 ns | 0.07 ns | 0.04 ns |  1.00 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate             | ShortRun   | 3              | 1           | 242.0 ns | 2.17 ns | 0.12 ns |  1.00 |         - |          NA |
| Generate_WithShardId | ShortRun   | 3              | 1           | 242.0 ns | 1.70 ns | 0.09 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 240.4 ns | 1.65 ns | 0.09 ns |  0.99 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 241.9 ns | 2.35 ns | 0.13 ns |  1.00 |         - |          NA |
