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
| CreatePosition |  6.885 ns |  2.800 ns | 0.1535 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 43.434 ns |  6.773 ns | 0.3713 ns |  6.31 |    0.13 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 33.062 ns | 15.296 ns | 0.8384 ns |  4.80 |    0.14 | 0.0091 |     152 B |        6.33 |
