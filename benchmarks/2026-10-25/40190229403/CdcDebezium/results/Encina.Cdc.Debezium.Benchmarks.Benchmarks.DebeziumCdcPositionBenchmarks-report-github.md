```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  4.751 ns |  2.188 ns | 0.1199 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 28.113 ns | 40.558 ns | 2.2231 ns |  5.92 |    0.43 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 21.245 ns | 15.252 ns | 0.8360 ns |  4.47 |    0.18 | 0.0091 |     152 B |        6.33 |
