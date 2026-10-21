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
| CreatePosition |  6.878 ns |  1.636 ns | 0.0897 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 38.040 ns | 20.622 ns | 1.1303 ns |  5.53 |    0.16 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.761 ns |  2.828 ns | 0.1550 ns |  4.47 |    0.05 | 0.0091 |     152 B |        6.33 |
