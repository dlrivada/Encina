```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.294 ns | 3.971 ns | 0.2177 ns |  1.00 |    0.06 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 25.213 ns | 5.405 ns | 0.2962 ns |  5.88 |    0.26 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 20.474 ns | 8.736 ns | 0.4789 ns |  4.78 |    0.23 | 0.0091 |     152 B |        6.33 |
