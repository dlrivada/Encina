```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.741 ns |  0.2132 ns | 0.0117 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 58.334 ns | 20.6054 ns | 1.1295 ns |  7.54 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 43.212 ns | 10.9868 ns | 0.6022 ns |  5.58 |    0.07 | 0.0091 |     152 B |        6.33 |
