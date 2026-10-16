```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.118 ns |  0.8183 ns | 0.0449 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 38.591 ns | 15.8514 ns | 0.8689 ns |  5.42 |    0.11 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.973 ns |  4.9262 ns | 0.2700 ns |  4.35 |    0.04 | 0.0091 |     152 B |        6.33 |
