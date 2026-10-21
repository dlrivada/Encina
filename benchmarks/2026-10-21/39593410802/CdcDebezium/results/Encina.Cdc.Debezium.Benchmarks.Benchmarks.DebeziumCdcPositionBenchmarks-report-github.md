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
| CreatePosition |  6.918 ns |  0.4077 ns | 0.0223 ns |  1.00 |    0.00 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 42.616 ns | 20.5334 ns | 1.1255 ns |  6.16 |    0.14 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 32.134 ns |  3.1206 ns | 0.1711 ns |  4.65 |    0.03 | 0.0091 |     152 B |        6.33 |
