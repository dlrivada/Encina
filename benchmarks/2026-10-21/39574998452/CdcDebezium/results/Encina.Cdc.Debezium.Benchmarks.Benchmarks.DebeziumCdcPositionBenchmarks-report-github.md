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
| CreatePosition |  4.325 ns |  1.376 ns | 0.0754 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 25.759 ns | 11.020 ns | 0.6040 ns |  5.96 |    0.15 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 21.647 ns |  8.710 ns | 0.4774 ns |  5.01 |    0.12 | 0.0091 |     152 B |        6.33 |
