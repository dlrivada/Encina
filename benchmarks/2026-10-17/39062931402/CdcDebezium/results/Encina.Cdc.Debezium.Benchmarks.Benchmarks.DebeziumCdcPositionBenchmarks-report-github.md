```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  6.778 ns | 3.7622 ns | 0.2062 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 37.648 ns | 1.5369 ns | 0.0842 ns |  5.56 |    0.14 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 30.701 ns | 0.6659 ns | 0.0365 ns |  4.53 |    0.12 | 0.0091 |     152 B |        6.33 |
