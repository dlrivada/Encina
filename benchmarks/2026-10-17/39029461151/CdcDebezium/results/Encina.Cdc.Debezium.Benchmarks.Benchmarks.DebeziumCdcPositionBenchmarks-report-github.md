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
| CreatePosition |  8.232 ns |  0.8643 ns | 0.0474 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 56.682 ns | 19.6282 ns | 1.0759 ns |  6.89 |    0.12 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 44.276 ns | 12.0112 ns | 0.6584 ns |  5.38 |    0.07 | 0.0091 |     152 B |        6.33 |
