```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.73GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method               | Job        | IterationCount | LaunchCount | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|--------------------- |----------- |--------------- |------------ |---------:|--------:|--------:|------:|----------:|------------:|
| Generate_WithShardId | Job-YFEFPZ | 10             | Default     | 242.1 ns | 0.07 ns | 0.05 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | Job-YFEFPZ | 10             | Default     | 242.1 ns | 0.02 ns | 0.01 ns |  1.00 |         - |          NA |
| Generate             | Job-YFEFPZ | 10             | Default     | 242.1 ns | 0.08 ns | 0.05 ns |  1.00 |         - |          NA |
| ExtractShardId       | Job-YFEFPZ | 10             | Default     | 240.4 ns | 0.05 ns | 0.03 ns |  0.99 |         - |          NA |
|                      |            |                |             |          |         |         |       |           |             |
| Generate_WithShardId | ShortRun   | 3              | 1           | 242.0 ns | 1.26 ns | 0.07 ns |  1.00 |         - |          NA |
| GenerateAndGetValue  | ShortRun   | 3              | 1           | 242.0 ns | 1.40 ns | 0.08 ns |  1.00 |         - |          NA |
| Generate             | ShortRun   | 3              | 1           | 242.0 ns | 1.60 ns | 0.09 ns |  1.00 |         - |          NA |
| ExtractShardId       | ShortRun   | 3              | 1           | 240.3 ns | 1.45 ns | 0.08 ns |  0.99 |         - |          NA |
