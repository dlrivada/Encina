```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  8.377 ns |  2.836 ns | 0.1554 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 58.651 ns | 72.834 ns | 3.9923 ns |  7.00 |    0.43 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 47.524 ns | 31.519 ns | 1.7276 ns |  5.67 |    0.20 | 0.0091 |     152 B |        6.33 |
