```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.065 ns |  2.506 ns | 0.1374 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 42.075 ns | 13.615 ns | 0.7463 ns |  5.96 |    0.14 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.838 ns |  2.535 ns | 0.1390 ns |  4.51 |    0.08 | 0.0091 |     152 B |        6.33 |
