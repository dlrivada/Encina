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
| CreatePosition |  7.068 ns |  2.348 ns | 0.1287 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 38.705 ns | 10.587 ns | 0.5803 ns |  5.48 |    0.11 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.592 ns |  2.741 ns | 0.1502 ns |  4.47 |    0.07 | 0.0091 |     152 B |        6.33 |
