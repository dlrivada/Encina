```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.355 ns |  1.818 ns | 0.0997 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 61.415 ns | 15.374 ns | 0.8427 ns |  7.35 |    0.12 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 46.911 ns |  3.070 ns | 0.1683 ns |  5.62 |    0.06 | 0.0091 |     152 B |        6.33 |
