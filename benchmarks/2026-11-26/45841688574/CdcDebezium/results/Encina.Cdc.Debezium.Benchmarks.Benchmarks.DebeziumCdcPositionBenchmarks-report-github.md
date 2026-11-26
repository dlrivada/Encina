```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method         | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------- |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreatePosition |  7.331 ns |  2.457 ns | 0.1347 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| FromBytes      | 42.003 ns | 18.330 ns | 1.0047 ns |  5.73 |    0.15 | 0.0181 |     304 B |       12.67 |
| ToBytes        | 31.602 ns | 17.334 ns | 0.9501 ns |  4.31 |    0.13 | 0.0091 |     152 B |        6.33 |
