```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error    | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|---------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.883 ns | 1.347 ns | 0.0738 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 38.870 ns | 4.996 ns | 0.2739 ns |  5.65 |    0.06 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.774 ns | 4.437 ns | 0.2432 ns |  4.47 |    0.05 | 0.0091 |     152 B |        6.33 |
