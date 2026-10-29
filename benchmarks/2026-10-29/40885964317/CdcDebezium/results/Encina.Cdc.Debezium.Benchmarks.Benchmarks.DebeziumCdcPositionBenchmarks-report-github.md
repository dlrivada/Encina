```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.918 ns |  2.369 ns | 0.1298 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 56.266 ns | 26.559 ns | 1.4558 ns |  7.11 |    0.19 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 43.382 ns | 15.988 ns | 0.8764 ns |  5.48 |    0.12 | 0.0091 |     152 B |        6.33 |
