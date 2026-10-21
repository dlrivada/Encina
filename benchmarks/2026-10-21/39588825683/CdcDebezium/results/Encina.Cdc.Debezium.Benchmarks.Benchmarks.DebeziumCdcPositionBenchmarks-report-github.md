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
| CreatePosition |  6.778 ns | 2.0784 ns | 0.1139 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 38.044 ns | 2.6919 ns | 0.1476 ns |  5.61 |    0.08 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.694 ns | 0.2941 ns | 0.0161 ns |  4.53 |    0.07 | 0.0091 |     152 B |        6.33 |
